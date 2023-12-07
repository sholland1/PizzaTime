using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace App;
public sealed class TypeRegistrar(IServiceCollection _builder) : ITypeRegistrar {
    public ITypeResolver Build() => new TypeResolver(_builder.BuildServiceProvider());
    public void Register(Type service, Type implementation) => _builder.AddSingleton(service, implementation);
    public void RegisterInstance(Type service, object implementation) => _builder.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> factory) {
        ArgumentNullException.ThrowIfNull(factory);
        _builder.AddSingleton(service, (provider) => factory());
    }
}

public sealed class TypeResolver(IServiceProvider _provider) : ITypeResolver, IDisposable {
    public object? Resolve(Type? type) {
        ArgumentNullException.ThrowIfNull(type);
        return _provider.GetService(type)!;
    }

    public void Dispose() {
        if (_provider is IDisposable disposable) {
            disposable.Dispose();
        }
    }
}
