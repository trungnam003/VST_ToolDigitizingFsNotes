using Microsoft.Extensions.DependencyInjection;
using VST_ToolDigitizingFsNotes.Libs.Common;

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
