#:property PublishAot=false
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:package Altcha@1.1.0

using System.Text.Json;
using Altcha;

var challenge = JsonSerializer.Deserialize<Challenge>(Console.ReadLine()!, AltchaJson.SerializerOptions)
    ?? throw new InvalidOperationException("Missing ALTCHA challenge.");
var solution = AltchaPow.SolveChallenge(new SolveChallengeOptions { Challenge = challenge });
var payload = new Payload { Challenge = challenge, Solution = solution };
Console.Write(Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(payload, AltchaJson.SerializerOptions)));
