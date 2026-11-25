// Merlin Test Applications
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Merlin.Extensions;
using Merlin.Replies;
using Wrap.Linq;

var app = new Merlin.App(args);

app.Router
	.Map("/")
		.Get(req => Reply.Text("Home"))
	.Map("/hello/{name}")
		.Get(req => from name in req.Route<string>("name")
					from text in Reply.Text($"Hello, {name}")
					select text)
;

await app.StartAsync();

