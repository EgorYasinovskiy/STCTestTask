var router = new Router.Core.Router();

router.RegisterRoute("/foo/bar/{p:int}/", (int p) => Console.WriteLine($"int: {p}"));
router.RegisterRoute("/foo/bar/{name:string}/", (string name) => Console.WriteLine($"name: {name}"));
router.RegisterRoute("/users/{id:guid}/", (Guid id) => Console.WriteLine($"guid: {id}"));
router.RegisterRoute("/items/{a:int}/{b:int}/", (int b, int a) => Console.WriteLine($"a={a}, b={b}"));
router.RegisterRoute("/static/path/", () => Console.WriteLine("static handler"));

router.Route("/foo/bar/42/");
router.Route("/foo/bar/hello/");
router.Route("/users/3f2504e0-4f89-11d3-9a0c-0305e82c3301/");
router.Route("/items/1/2/");
router.Route("/static/path/");

try { router.Route("/not/found/"); }
catch (InvalidOperationException ex) { Console.WriteLine("404: " + ex.Message); }
