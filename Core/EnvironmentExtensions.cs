namespace Cloudy
{
    static class EnvironmentExtensions
    {
        /// <summary>
        /// Whether azure functions are running in development mode, locally.
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public static bool IsDevelopment(this IEnvironment env)
            => IsTesting(env) || env.GetVariable("AZURE_FUNCTIONS_ENVIRONMENT", "Production") == "Development";

        /// <summary>
        /// Whether the code is being run from a test.
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public static bool IsTesting(this IEnvironment env)
            => env.GetVariable("TESTING", false);
    }
}
