// See https://aka.ms/new-console-template for more information
using System.Runtime.CompilerServices;
using MemoryPack;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.NodaTime;
using MessagePack.Resolvers;
using NodaTime;
using Vogen;
var resolver = CompositeResolver.Create(
    NodatimeResolver.Instance,
    ContractlessStandardResolver.Instance
);
MessagePackSerializer.DefaultOptions = new(resolver);
var r = new TestRequest(1, 2, SystemClock.Instance.GetCurrentInstant());
Console.WriteLine($"r: {r}");
var bytes = MessagePackSerializer.Serialize(r);
Console.WriteLine($"bytes: {bytes.Length}");
var mpBytes = MemoryPackSerializer.Serialize(r);
Console.WriteLine($"mpBytes: {mpBytes.Length}");
var d = MessagePackSerializer.Deserialize<TestRequest>(bytes);
Console.WriteLine($"d: {d}");
var mpD = MemoryPackSerializer.Deserialize<TestRequest>(mpBytes);
Console.WriteLine($"mpD: {mpD}");

Console.WriteLine("Hello, World!");

[MemoryPackable]
public partial record TestRequest(
    int X,
    int Y,
    Instant Now
);

[ValueObject<string>]
[MessagePackFormatter(typeof(SomeVoMessagePackFormatter))]
public readonly partial struct SomeVo;

public class SomeVoMessagePackFormatter : IMessagePackFormatter<SomeVo>
{
    public void Serialize(ref MessagePackWriter writer, SomeVo value, MessagePackSerializerOptions options) => writer.Write(value.Value);

    public SomeVo Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil()) throw new NotImplementedException(); // this must be some more general for sure :)

        options.Security.DepthStep(ref reader);

        var value = reader.ReadString();

        reader.Depth--;
        return SomeVo.From(value!);
    }
}

public class Foo
{
    [ModuleInitializer]
    public static void Register()
    {
        Console.WriteLine("hahaha I ran");
    }
}