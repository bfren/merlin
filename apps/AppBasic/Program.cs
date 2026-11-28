// Merlin Test Applications
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using System.Text.Json.Serialization;
using Merlin;
using Merlin.Domain;
using Merlin.Extensions;
using Merlin.Replies;
using Microsoft.Extensions.DependencyInjection;
using Wrap;
using Wrap.Linq;

var app = new App(args, AppJson.Default);
app.Register<ISpeak, Speak>();

static Task<Result<Reply>> Home(Req req) =>
	Reply.Text("Home").AsTask();

static HandlerAsync SayHello(ISpeak speak) =>
	req => from name in req.Route<string>("name")
		   from text in R.TryAsync(() => speak.SayHello(name))
		   from reply in Reply.Text(text)
		   select reply;

static Result<Reply> CalculateAgeInDays(Req req) =>
	from dob in req.Route<DateTimeOffset>("dob")
	from age in R.Try(() => (DateTime.Now.Date - dob.Date).Days)
	from text in R.Try(() => $"You are {age} days old.")
	from reply in Reply.Text(text)
	select reply;

app.Router
	.Map("/")
		.Get(Home)
	.Map("/hello/{name}")
		.Get(sp => SayHello(sp.GetRequiredService<ISpeak>()))
		.Post(req => from name in req.Route<string>("name")
					 from json in R.Wrap(new Greeting(name))
					 from reply in Reply.Json(json)
					 select reply)
	.Map("/age/{dob:datetime}")
		.Get(CalculateAgeInDays);
;

await app.StartAsync();

internal interface ISpeak
{
	Task<string> SayHello(string name);
}

internal sealed class Speak : ISpeak
{
	public Task<string> SayHello(string name) =>
		Task.FromResult($"Hello, {name}!");
}

internal readonly record struct Greeting(string Hello);

[JsonSerializable(typeof(Greeting))]
internal partial class AppJson : JsonSerializerContext;
