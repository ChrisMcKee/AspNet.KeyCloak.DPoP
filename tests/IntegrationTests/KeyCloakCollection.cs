namespace IntegrationTests;

/// <summary>
/// xUnit collection that shares a single Keycloak container across all integration test classes.
/// The container starts once and stays alive for the duration of the test run.
/// </summary>
[CollectionDefinition(Name)]
public class KeyCloakCollection : ICollectionFixture<KeyCloakFixture>
{
    public const string Name = "KeyCloak";
}
