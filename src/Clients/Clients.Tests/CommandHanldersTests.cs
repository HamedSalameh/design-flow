﻿using Clients.Application.Commands;
using Clients.Domain.Entities;
using Clients.Domain.ValueObjects;
using Clients.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Clients.Tests
{
    [TestFixture]
    public class CreateClientCommandHandlerTests
    {
        private Mock<ILogger<CreateClientCommandHandler>> _loggerMock;
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private CreateClientCommandHandler _handler;

        readonly Guid Tenant = Guid.NewGuid();
        readonly string firstName = "John";
        readonly string familyName = "Doe";
        readonly string primaryPhoneNumber = "123-9222333";
        readonly string secondaryPhoneNumber = "12-9987878";
        readonly string emailAddress = "someaddress@mailserver.com";
        readonly string street = "SomeStreet";
        readonly string city = "cityName";
        readonly string buildingNumber = "bn-05";
        readonly List<string> addressLines = new() { "address line1", "address line2" };

        private ContactDetails contactDetails;
        private Address address;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<CreateClientCommandHandler>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _handler = new CreateClientCommandHandler(_loggerMock.Object, _unitOfWorkMock.Object);

            contactDetails = new ContactDetails(primaryPhoneNumber, secondaryPhoneNumber, emailAddress);
            address = new Address(city, street, buildingNumber, addressLines);
        }

        [Test]
        public async Task Handle_ValidRequest_ReturnsClientId()
        {
            // Arrange
            var draftClient = new Client(firstName, familyName, address, contactDetails, Tenant);
            var request = new CreateClientCommand(draftClient);

            _unitOfWorkMock.Setup(uow => uow.ClientsRepository.CreateClientAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Handle_NullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            ILogger<CreateClientCommandHandler>? logger = null;
            var unitOfWork = new Mock<IUnitOfWork>().Object;

            // Act + Assert
#pragma warning disable CS8604 // Possible null reference argument.
            _ = Assert.Throws<ArgumentNullException>(() => new CreateClientCommandHandler(logger, unitOfWork));
#pragma warning restore CS8604 // Possible null reference argument.
        }

        [Test]
        public void Handle_NullUnitOfWork_ThrowsArgumentNullException()
        {
            // Arrange
            var logger = new Mock<ILogger<CreateClientCommandHandler>>().Object;
            IUnitOfWork? unitOfWork = null;

            // Act + Assert
#pragma warning disable CS8604 // Possible null reference argument.
            _ = Assert.Throws<ArgumentNullException>(() => new CreateClientCommandHandler(logger, unitOfWork));
#pragma warning restore CS8604 // Possible null reference argument.
        }
    }
}
