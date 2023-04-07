using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using static BuilderHelpers;

public static class PizzaSerializer {
    public static JsonSerializerOptions Options => new() {
        WriteIndented = true,
        Converters = {
            new JsonStringEnumConverter(),
            new CheeseJsonConverter(),
            new SauceJsonConverter(),
            new ToppingJsonConverter(),
            new PaymentJsonConverter(),
            new ServiceMethodJsonConverter(),
            new OrderTimingJsonCoverter(),
            new OptionsJsonConverter(),
        }
    };
}

public class OptionsJsonConverter : JsonConverter<DominosCart.Options> {
    public override DominosCart.Options? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions _) {
        if (reader.TokenType != JsonTokenType.StartObject) {
            throw new NotSupportedException($"Invalid Options! Token: {reader.TokenType}");
        }

        var options = new Dictionary<string, Dictionary<string, string>?>();
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject) {
                break;
            }
            if (reader.TokenType == JsonTokenType.PropertyName) {
                var option = reader.GetString()!;
                reader.Read();
                if (reader.TokenType == JsonTokenType.Number) {
                    options.Add(option, null);
                }
                else {
                    var optionValues = new Dictionary<string, string>();
                    while (reader.Read()) {
                        if (reader.TokenType == JsonTokenType.PropertyName) {
                            var optionValue = reader.GetString()!;
                            reader.Read();
                            optionValues.Add(optionValue, reader.GetString()!);
                        }

                        if (reader.TokenType == JsonTokenType.EndObject) {
                            options.Add(option, optionValues);
                            break;
                        }
                    }
                }
            }
        }

        return new(options);
    }

    public override void Write(Utf8JsonWriter writer, DominosCart.Options value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        foreach (var (option, optionValues) in value) {
            writer.WritePropertyName(option);
            if (optionValues is null) {
                writer.WriteNumberValue(0);
            } else {
                writer.WriteStartObject();
                foreach (var (optionValue, optionValueName) in optionValues) {
                    writer.WriteString(optionValue, optionValueName);
                }
                writer.WriteEndObject();
            }
        }
        writer.WriteEndObject();
    }
}

public class OrderTimingJsonCoverter : JsonConverter<OrderTiming> {
    public override OrderTiming? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var value = reader.GetString()!;
        return value == "Now"
            ? new OrderTiming.Now()
            : new OrderTiming.Later(DateTime.Parse(value));

        throw new NotSupportedException($"Invalid OrderTiming! Value: {value}");
    }

    public override void Write(Utf8JsonWriter writer, OrderTiming value, JsonSerializerOptions options) =>
        value.Match(
            () => writer.WriteStringValue("Now"),
            later => writer.WriteStringValue(later));
}

public class ServiceMethodJsonConverter : JsonConverter<ServiceMethod> {
    public override ServiceMethod? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.StartObject) {
            return ReadDelivery(ref reader);
        }

        if (reader.TokenType == JsonTokenType.String) {
            var value = reader.GetString()!;
            return value.StartsWith("Carryout")
                ? new ServiceMethod.Carryout(Enum.Parse<PickupLocation>(value.Replace("Carryout - ", "")))
                : throw new NotSupportedException($"Invalid ServiceMethod! Value: {value}");
        }

        throw new NotSupportedException($"Invalid ServiceMethod! Token: {reader.TokenType}");
    }

    private static ServiceMethod.Delivery ReadDelivery(ref Utf8JsonReader reader) {
        AddressType AddressType = default;
        string? Name = null;
        string? StreetAddress = null;
        int? Apt = null;
        string? ZipCode = null;
        string? City = null;
        string? State = null;

        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.PropertyName) {
                var property = reader.GetString();
                reader.Read();
                switch (property) {
                    case nameof(AddressType):
                        AddressType = Enum.Parse<AddressType>(reader.GetString()!);
                        break;
                    case nameof(Name):
                        Name = reader.GetString();
                        break;
                    case nameof(StreetAddress):
                        StreetAddress = reader.GetString();
                        break;
                    case nameof(Apt):
                        Apt = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();
                        break;
                    case nameof(ZipCode):
                        ZipCode = reader.GetString();
                        break;
                    case nameof(City):
                        City = reader.GetString();
                        break;
                    case nameof(State):
                        State = reader.GetString();
                        break;
                    default:
                        throw new NotSupportedException($"Invalid Property! Property: {property}");
                }
            }

            if (reader.TokenType == JsonTokenType.EndObject) {
                return new(
                    new(AddressType, Name, StreetAddress!, Apt, ZipCode!, City!, State!));
            }
        }

        throw new NotSupportedException($"Invalid ServiceMethod! Token: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, ServiceMethod value, JsonSerializerOptions options) =>
        value.Match(
            address => JsonSerializer.Serialize(writer, address, options),
            location => writer.WriteStringValue($"Carryout - {location}"));
}

public class PaymentJsonConverter : JsonConverter<Payment> {
    public override Payment? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.String) {
            var value = reader.GetString();
            return value == "PayAtStore"
                ? new Payment.PayAtStore()
                : throw new NotSupportedException($"Invalid Payment! Value: {value}");
        }

        if (reader.TokenType == JsonTokenType.StartObject) {
            return ReadPayWithCard(ref reader);
        }

        throw new NotSupportedException($"Invalid Payment! Token: {reader.TokenType}");
    }

    private static Payment.PayWithCard ReadPayWithCard(ref Utf8JsonReader reader) {
        var CardNumber = 0L;
        string? Expiration = null;
        string? SecurityCode = null;
        string? BillingZip = null;

        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.PropertyName) {
                var property = reader.GetString();
                reader.Read();
                switch (property) {
                    case nameof(CardNumber):
                        CardNumber = reader.GetInt64();
                        break;
                    case nameof(Expiration):
                        Expiration = reader.GetString();
                        break;
                    case nameof(SecurityCode):
                        SecurityCode = reader.GetString();
                        break;
                    case nameof(BillingZip):
                        BillingZip = reader.GetString();
                        break;
                    default:
                        throw new NotSupportedException($"Invalid Payment.PayWithCard property! Property: {property}");
                }
            }

            if (reader.TokenType == JsonTokenType.EndObject) {
                return new(CardNumber, Expiration!, SecurityCode!, BillingZip!);
            }
        }

        throw new NotSupportedException($"Invalid Payment.PayWithCard! Token: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, Payment value, JsonSerializerOptions options) {
        if (value is Payment.PayAtStore p) {
            writer.WriteStringValue("PayAtStore");
            return;
        }
        
        if (value is Payment.PayWithCard pp) {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(pp.CardNumber));
            writer.WriteNumberValue(pp.CardNumber);
            writer.WritePropertyName(nameof(pp.Expiration));
            writer.WriteStringValue(pp.Expiration);
            writer.WritePropertyName(nameof(pp.SecurityCode));
            writer.WriteStringValue(pp.SecurityCode);
            writer.WritePropertyName(nameof(pp.BillingZip));
            writer.WriteStringValue(pp.BillingZip);
            writer.WriteEndObject();
            return;
        }

        throw new NotSupportedException($"Invalid Payment! Value: {value}");
    }
}

static class AmountUtils {
    public static char Serialize(this Amount? amount) =>
        amount.HasValue ? amount.Value.Serialize() : '_';

    public static char Serialize(this Amount amount) => amount switch {
        Amount.Light => '-',
        Amount.Normal => '=',
        Amount.Extra => '^',
        _ => throw new UnreachableException($"Invalid amount! Value: {amount}"),
    };

    public static Amount? Deserialize(char amount) => amount switch {
        '-' => Light,
        '=' => Normal,
        '^' => Extra,
        '_' => None,
        _ => throw new NotSupportedException($"Invalid amount! Value: {amount}")
    };
}

static class LocationUtils {
    public static char Serialize(this Location location) => Enum.GetName(location)![0];

    public static Location Deserialize(char location) => location switch {
        'A' => All,
        'L' => Left,
        'R' => Right,
        _ => throw new NotSupportedException($"Invalid location! Value: {location}")
    };
}

public class CheeseJsonConverter : JsonConverter<Cheese> {
    public override Cheese Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var value = reader.GetString()!;
        return value switch {
            [var x, var y] => new Cheese.Sides(
                AmountUtils.Deserialize(x),
                AmountUtils.Deserialize(y)),
            ['_'] => new Cheese.None(),
            [var x] => new Cheese.Full(AmountUtils.Deserialize(x)!.Value),
            _ => throw new NotSupportedException($"Invalid cheese! Value: {value}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Cheese value, JsonSerializerOptions options) {
        var x = value.Match(
            amt => amt.Serialize().ToString(),
            (left, right) => $"{left.Serialize()}{right.Serialize()}",
            () => "_");
        writer.WriteStringValue(x);
    }
}

public class SauceJsonConverter : JsonConverter<Sauce> {
    public override Sauce Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var value = reader.GetString()!;

        var amount = AmountUtils.Deserialize(value[0])!.Value;
        var sauceType = Enum.Parse<SauceType>(value[1..]);

        return new(sauceType, amount);
    }

    public override void Write(Utf8JsonWriter writer, Sauce value, JsonSerializerOptions options) =>
        writer.WriteStringValue(
            $"{value.Amount.Serialize()}{Enum.GetName(value.SauceType)}");
}

public class ToppingJsonConverter : JsonConverter<Topping> {
    public override Topping Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var value = reader.GetString()!;

        var location = LocationUtils.Deserialize(value[0]);
        var amount = AmountUtils.Deserialize(value[1])!.Value;
        var toppingType = Enum.Parse<ToppingType>(value[2..]);

        return new(toppingType, location, amount);
    }

    public override void Write(Utf8JsonWriter writer, Topping value, JsonSerializerOptions options) {
        var loc = value.Location.Serialize();
        var amt = value.Amount.Serialize();
        writer.WriteStringValue($"{loc}{amt}{Enum.GetName(value.ToppingType)}");
    }
}
