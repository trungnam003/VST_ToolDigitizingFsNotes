using Microsoft.Extensions.DependencyInjection;

namespace VST_ToolDigitizingFsNotes.Libs
{
    public static class DependencyInjection
    {

        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining<TrungNam>();
            });

            return services;
        }
    }
}
