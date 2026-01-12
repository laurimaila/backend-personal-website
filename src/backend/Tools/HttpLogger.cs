using Microsoft.AspNetCore.HttpLogging;

namespace backend.Tools;
public static class HttpLogger
{
    public static void ConfigureHttpLogging(IServiceCollection services, bool isDevelopment)
    {
        services.AddHttpLogging(logging =>
        {
            if (isDevelopment)
            {
                logging.LoggingFields = HttpLoggingFields.RequestMethod |
                                        HttpLoggingFields.RequestScheme |
                                        HttpLoggingFields.RequestPath |
                                        HttpLoggingFields.RequestBody |
                                        HttpLoggingFields.ResponseStatusCode |
                                        HttpLoggingFields.ResponseBody;
            }
            else
            {
                logging.LoggingFields = HttpLoggingFields.RequestMethod |
                                        HttpLoggingFields.RequestScheme |
                                        HttpLoggingFields.RequestPath |
                                        HttpLoggingFields.ResponseStatusCode;
            }

            logging.CombineLogs = true;
        });
    }
}
