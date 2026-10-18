using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class ReferenceCollectionRoles
{
    [TestMethod]
    public void concurrent_opposite_roles_accumulate_and_repeated_registrations_preserve_both()
    {
        var prefix = Guid.NewGuid().ToString();
        const int collectionCount = 128;

        Parallel.For(0, collectionCount * 2, i =>
        {
            var name = prefix + i / 2;
            var role = i % 2 == 0 ? JoinRecordRole.Parent : JoinRecordRole.Child;
            Cache<RegistrationEntity>.AddReferenceCollection(name, role);
        });

        for (var i = 0; i < collectionCount; i++)
        {
            var name = prefix + i;
            Assert.AreEqual(JoinRecordRole.Both, Cache<RegistrationEntity>.ReferenceCollections[name]);

            Cache<RegistrationEntity>.AddReferenceCollection(name, JoinRecordRole.Parent);
            Cache<RegistrationEntity>.AddReferenceCollection(name, JoinRecordRole.Child);
            Cache<RegistrationEntity>.AddReferenceCollection(name, JoinRecordRole.Both);

            Assert.AreEqual(JoinRecordRole.Both, Cache<RegistrationEntity>.ReferenceCollections[name]);
        }
    }

    [TestMethod]
    public void combined_role_registration_adds_missing_flags()
    {
        foreach (var role in new[] { JoinRecordRole.Parent, JoinRecordRole.Child })
        {
            var name = Guid.NewGuid().ToString();
            Cache<RegistrationEntity>.AddReferenceCollection(name, role);
            Assert.AreEqual(role, Cache<RegistrationEntity>.ReferenceCollections[name]);

            Cache<RegistrationEntity>.AddReferenceCollection(name, JoinRecordRole.Both);

            Assert.AreEqual(JoinRecordRole.Both, Cache<RegistrationEntity>.ReferenceCollections[name]);
        }
    }

    public sealed class RegistrationEntity : Entity { }
}
