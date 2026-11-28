// Merlin Test Applications
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin;
using Merlin.Extensions;
using Merlin.Replies;
using Wrap;
using Wrap.Linq;

var app = new App(args);

static Task<Result<Reply>> Home(Req req) =>
	Reply.Text("Home").AsTask();

app.Router
	.Map("/")
		.Get(sp => req => Home(req))
	.Map("/hello/{name}")
		.Get(req =>
			from name in req.Route<string>("name")
			from text in Reply.Text($"Hello, {name}!")
			select text)
		.Post(req =>
			from name in req.Route<string>("name")
			from json in Reply.Json(new { Hello = name })
			select json)
;

await app.StartAsync();

