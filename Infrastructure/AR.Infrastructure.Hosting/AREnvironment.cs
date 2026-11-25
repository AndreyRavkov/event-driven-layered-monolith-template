namespace AR.Infrastructure.Hosting;

public static class AREnvironment
{
    private const string AzureFuncEnvironmentName = "AZURE_FUNCTIONS_ENVIRONMENT";
    private const string AspNetCoreEnvironmentName = "ASPNETCORE_ENVIRONMENT";

    private const string DevelopmentEnvironmentValue = "Development";
    private const string QaEnvironmentValue = "qa";
    private const string ProductionEnvironmentValue = "prod";

    private static readonly string EnvironmentVariable =
        Environment.GetEnvironmentVariable(AzureFuncEnvironmentName)
        ?? Environment.GetEnvironmentVariable(AspNetCoreEnvironmentName)
        ?? DevelopmentEnvironmentValue;

    public static bool IsLocal() =>
        string.Equals(EnvironmentVariable, DevelopmentEnvironmentValue, StringComparison.InvariantCultureIgnoreCase);

    public static bool IsQa() =>
        string.Equals(EnvironmentVariable, QaEnvironmentValue, StringComparison.InvariantCultureIgnoreCase);

    public static bool IsProduction() =>
        string.Equals(EnvironmentVariable, ProductionEnvironmentValue, StringComparison.InvariantCultureIgnoreCase);

    public static string GetEnvironmentName() => EnvironmentVariable;
}