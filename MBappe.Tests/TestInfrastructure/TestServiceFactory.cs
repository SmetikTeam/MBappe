namespace MBappe.Tests.TestInfrastructure;

public static class TestServiceFactory
{
    public static TestAppServices Create()
    {
        return new TestAppServices();
    }
}
