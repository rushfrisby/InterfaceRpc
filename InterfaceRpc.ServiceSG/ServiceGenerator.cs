using System;
using Microsoft.CodeAnalysis;

namespace InterfaceRpc.ServiceSG
{
    [Generator]
    public class ServiceGenerator : ISourceGenerator
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceGenerator(IWebApplicationBuilder builder)
        {

        }

        public void Initialize(GeneratorInitializationContext context)
        {
            throw new NotImplementedException();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
