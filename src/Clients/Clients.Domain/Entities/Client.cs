﻿
using Clients.Domain.ValueObjects;
using System.Text;

namespace Clients.Domain.Entities
{
    public class Client : Entity
    {
        public string FirstName { get; set; }
        public string FamilyName { get; set; }

        public Address Address { get; set; }

        public ContactDetails ContactDetails { get; set; }

        public Client(string firstName, string familyName, Address address, ContactDetails contactDetails)
        {
            if (string.IsNullOrEmpty(firstName))
            {
                throw new ArgumentException($"{nameof(firstName)} : must not be null or empty");
            }
            Id = Guid.NewGuid();
            FirstName = firstName;
            FamilyName = familyName;
            Address = address ?? throw new ArgumentNullException(nameof(address));
            ContactDetails = contactDetails ?? throw new ArgumentNullException(nameof(contactDetails));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Internal use by EF Core")]
        private Client(string firstName, string familyName)
        {
            FirstName = firstName;
            FamilyName = familyName;

            ContactDetails = new ContactDetails(Consts.Strings.ValueNotSet);
            Address = new Address(Consts.Strings.ValueNotSet);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(FirstName);
            if (!string.IsNullOrEmpty(FamilyName)) sb.Append(" ").Append(FamilyName);

            sb.Append(", ").Append(Address.ToString());
            sb.Append(", ").Append(ContactDetails.ToString());

            return sb.ToString();
        }
    }
}