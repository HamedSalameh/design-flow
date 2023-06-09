﻿using Clients.Domain;
using Clients.Domain.Entities;
using Clients.Domain.ValueObjects;
using Clients.Infrastructure.Interfaces;
using Clients.Infrastructure.Polly;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using Polly;
using Polly.Wrap;
using SharedKernel.ConnectionProviders;
using SqlKata;
using SqlKata.Compilers;
using System.Data;

namespace Clients.Infrastructure.Persistance
{
    internal class ClientsRepository : IClientsRepository
    {
        private readonly ClientsDBContext _dbContext;
        private readonly ILogger<ClientsRepository> _logger;
        private readonly IDbConnectionStringProvider dbConnectionStringProvider;
        private readonly AsyncPolicyWrap policy;

        public ClientsRepository(ClientsDBContext dbContext, ILogger<ClientsRepository> logger, IDbConnectionStringProvider dbConnectionStringProvider)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbConnectionStringProvider = dbConnectionStringProvider;

            DefaultTypeMap.MatchNamesWithUnderscores = true;
            SqlMapper.AddTypeHandler(new JsonbTypeHandler<List<string>>());
            policy = PollyPolicyFactory.WrappedAsyncPolicies();
        }

        public async Task<Guid> CreateClientAsync(Client client, CancellationToken cancellationToken)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            await _dbContext.Clients.AddAsync(client, cancellationToken).ConfigureAwait(false);

            _ = policy.ExecuteAsync(async () => await _dbContext.SaveChangesAsync().ConfigureAwait(false));

            _logger.LogDebug("Client {client.Id} was successfully created.", client.Id);

            return client.Id;
        }

        public async Task<Client> UpdateClientAsync(Client client, CancellationToken cancellationToken)
        {
            if (client == default || client == null)
            {
                _logger.LogError($"Invalid value for {nameof(client)}: {client}");
                throw new ArgumentException($"Invalid value of client object");
            }
            if (client.Id == default)
            {
                _logger.LogError($"Invalid value for {nameof(client.Id)}: {client.Id}");
                throw new ArgumentException("Client object has invalid value for Id property.");
            }

            var parameters = new DynamicParameters();
            parameters.Add("p_id", client.Id, DbType.Guid);
            parameters.Add("p_tenant_id", client.TenantId, DbType.Guid);
            parameters.Add("p_first_name", client.FirstName, DbType.String);
            parameters.Add("p_family_name", client.FamilyName, DbType.String);
            parameters.Add("p_city", client.Address.City, DbType.String);
            parameters.Add("p_street", client.Address.Street, DbType.String);
            parameters.Add("p_building_number", client.Address.BuildingNumber, DbType.String);
            parameters.Add("p_address_lines", JsonConvert.SerializeObject(client.Address.AddressLines));
            parameters.Add("p_primary_phone_number", client.ContactDetails.PrimaryPhoneNumber, DbType.String);
            parameters.Add("p_secondary_phone_number", client.ContactDetails.SecondaryPhoneNumber, DbType.String);
            parameters.Add("p_email_address", client.ContactDetails.EmailAddress, DbType.String);

            using (var connection = new NpgsqlConnection(dbConnectionStringProvider.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken);
                using var transaction = connection.BeginTransaction();
                try
                {
                    await connection.ExecuteAsync("update_client", parameters,
                        transaction: transaction, commandType: CommandType.StoredProcedure);
                    transaction.Commit();
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Could not update client entity due to error : {exception.Message}");
                    transaction.Rollback();
                    throw;
                }
            }

            return client;
        }

        public async Task DeleteClientAsync(Guid id, CancellationToken cancellationToken)
        {
            if (id == default || id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var policy = PollyPolicyFactory.WrappedAsyncPolicies();
            var entity = await _dbContext.Clients
                .FindAsync(new object?[] { id, cancellationToken }, cancellationToken: cancellationToken);

            if (entity == null)
            {
                throw new EntityNotFoundException(id.ToString());
            }

            _dbContext.Remove(entity);
            _ = policy.ExecuteAsync(async () => await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false));
            _logger.LogDebug("Delete client: {id}", id);
        }

        public async Task<Client?> GetClientAsyncNoTracking(Guid id, CancellationToken cancellationToken)
        {
            if (id == default || id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var client = await _dbContext.Clients.AsNoTracking()
                .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
                .ConfigureAwait(false);

            return client;
        }

        public async Task<Client?> GetClientAsyncWithDapper(Guid id, CancellationToken cancellationToken)
        {
            var sqlCommand = "SELECT * FROM clients WHERE id=@id";

            var dynamic = new DynamicParameters();
            dynamic.Add(nameof(id), id);

            _logger.LogDebug("{sqlCommand} : {sqlParameters}", sqlCommand, dynamic);
            using (var connection = new NpgsqlConnection(dbConnectionStringProvider.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken);

                var client = await policy.ExecuteAsync(async () =>
                {
                    var result = await connection.QueryAsync<Client, Address, ContactDetails, Client>(
                    new CommandDefinition(sqlCommand, parameters: dynamic, cancellationToken: cancellationToken),
                    (client, address, contactDetails) =>
                    {
                        client.Address = address;
                        client.ContactDetails = contactDetails;
                        return client;
                    }, "city, primary_phone_number")        // since we are using value objects from different class, we need to set the split/join on field.
                    .ConfigureAwait(false);

                    return result.FirstOrDefault();
                });

                return client;
            };
        }

        public async Task<IEnumerable<Client>> SearchClientsAsync(string firstName, string familyName, string city, CancellationToken cancellationToken)
        {
            var query = new Query("clients")
                .Select();

            if (!string.IsNullOrEmpty(firstName))
            {
                query.WhereContains("first_name", firstName);
            }
            if (!string.IsNullOrEmpty(familyName))
            {
                query.WhereContains("family_name", familyName);
            }
            if (!string.IsNullOrEmpty(city))
            {
                query.WhereContains("city", city);
            }

            using (var connection = new NpgsqlConnection(dbConnectionStringProvider.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken);

                return await policy.ExecuteAsync(async () =>
                {
                    var compiler = new PostgresCompiler();
                    var generatedQuery = compiler.Compile(query);
                    var result = await connection.QueryAsync<Client, Address, ContactDetails, Client>(
                        generatedQuery.Sql,
                        (client, address, contactDetails) =>
                        {
                            client.Address = address;
                            client.ContactDetails = contactDetails;
                            return client;
                        },
                        param: generatedQuery.NamedBindings,
                        splitOn: "city, primary_phone_number");
                    return result;
                });
            };
        }
    }
}
