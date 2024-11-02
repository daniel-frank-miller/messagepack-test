// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MemoryPack;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.NodaTime;
using MessagePack.Resolvers;
using NodaTime;
using Vogen;

BenchmarkRunner.Run<MyBenchs>();
// var resolver = CompositeResolver.Create(
//     NodatimeResolver.Instance,
//     ContractlessStandardResolver.Instance
// );
// StaticCompositeResolver.Instance.Register(
//     CompositeResolver.Create(new FuckMessagePackFormatter()),
//     NodatimeResolver.Instance,
//     ContractlessStandardResolver.Instance
// );
// MessagePackSerializer.DefaultOptions = new(StaticCompositeResolver.Instance);
// var r = new TestRequest(1, 2, Fuck.From(3), SystemClock.Instance.GetCurrentInstant());
// Console.WriteLine($"r: {r}");

// var bytes = MessagePackSerializer.Serialize(r);
// Console.WriteLine($"bytes: {bytes.Length}");

// // var mpBytes = MemoryPackSerializer.Serialize(r);
// // Console.WriteLine($"mpBytes: {mpBytes.Length}");

// var d = MessagePackSerializer.Deserialize<TestRequest>(bytes);
// Console.WriteLine($"d: {d}");

// // var mpD = MemoryPackSerializer.Deserialize<TestRequest>(mpBytes);
// // Console.WriteLine($"mpD: {mpD}");

Console.WriteLine("Hello, World!");

// [MemoryPackable]
public partial record TestRequest(
    int X,
    int Y,
    Fuck Fuck,
    Instant Now
);
[MemoryPackable]
public partial record Another(
    int X
);

// [ValueObject<string>]
// [MessagePackFormatter(typeof(SomeVoMessagePackFormatter))]
public readonly record struct SomeVo(int Value);

[ValueObject<int>]
public readonly partial struct Fuck : IValueObject<Fuck, int>;

public class FuckMessagePackFormatter() : IMessagePackFormatter<Fuck>
{
    public void Serialize(ref MessagePackWriter writer, Fuck value, MessagePackSerializerOptions options)
    {
        Console.WriteLine("Serializing");
        writer.Write(value.Value);
    }

    public Fuck Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        Console.WriteLine("Deserializing");
        if (reader.TryReadNil()) throw new NotImplementedException(); // this must be some more general for sure :)

        options.Security.DepthStep(ref reader);

        var value = reader.ReadInt32();

        reader.Depth--;
        return Fuck.From(value!);
    }

    [ModuleInitializer]
    public static void Register()
    {
        Console.WriteLine("Registered");
    }
}

// public class IPAddressMsgPackFormatter

public class IPAddressFormatter : MemoryPackCustomFormatterAttribute<IPAddress>
{
    public override IMemoryPackFormatter<IPAddress> GetFormatter() => new Formatter();

    private class Formatter : MemoryPackFormatter<IPAddress>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref IPAddress? value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteValue(value.ToString());
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref IPAddress? value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = IPAddress.Parse(reader.ReadString()!);
        }
    }
}

public interface IValueObject<TSelf, TPrimitive>
{
    abstract static TSelf From(TPrimitive primitive);
    TPrimitive Value { get; }
}

[AttributeUsage(AttributeTargets.All)]
public class ValueObjectFormatter<TVo, TPrimitive> : MemoryPackCustomFormatterAttribute<TVo>
    where TVo : IValueObject<TVo, TPrimitive>
{
    public override IMemoryPackFormatter<TVo> GetFormatter() => new Formatter();

    private class Formatter : MemoryPackFormatter<TVo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TVo value)
        {
            writer.WriteValue(value.Value);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TVo value)
        {
            value = TVo.From(reader.ReadValue<TPrimitive>()!);
        }
    }
}

[MemoryDiagnoser(false)]
[MediumRunJob]
public class MyBenchs
{
    private readonly TestRequest _testRequest = new(1, 2, Fuck.From(3), SystemClock.Instance.GetCurrentInstant());
    private readonly MessagePackSerializerOptions _options;
    public MyBenchs()
    {
        StaticCompositeResolver.Instance.Register(
            CompositeResolver.Create(new FuckMessagePackFormatter()),
            NodatimeResolver.Instance,
            ContractlessStandardResolver.Instance
        );
        _options = new(StaticCompositeResolver.Instance);
    }
    [Benchmark(Baseline = true)]
    public byte[] SystemTextJson()
    {
        return JsonSerializer.SerializeToUtf8Bytes(_testRequest);
    }

    [Benchmark]
    public byte[] MessagePack()
    {
        return MessagePackSerializer.Serialize(_testRequest, _options);
    }
}