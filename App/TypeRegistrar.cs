using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace App;
public sealed class TypeRegistrar(IServiceCollection builder) : ITypeRegistrar {
    public ITypeResolver Build() => new TypeResolver(builder.BuildServiceProvider());
    public void Register(Type service, Type implementation) => builder.AddSingleton(service, implementation);
    public void RegisterInstance(Type service, object implementation) => builder.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> factory) {
        ArgumentNullException.ThrowIfNull(factory);
        builder.AddSingleton(service, (provider) => factory());
    }
}

public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable {
    public object? Resolve(Type? type) {
        ArgumentNullException.ThrowIfNull(type);
        return provider.GetService(type)!;
    }

    public void Dispose() {
        if (provider is IDisposable disposable) {
            disposable.Dispose();
        }
    }
}
