# Handlebars.Net.Mvc
An ASP.NET MVC ViewEngine using the Handlebars syntax.

## About
This project uses https://github.com/rexm/Handlebars.Net to compile the view files on the server to .NET bytecode.
The syntax for the view files is described at http://handlebarsjs.com/ (but note that is client-side Javascript so some parts differ).

## Documentation
The API documentation exists at https://jiellse.github.io/Handlebars.Net.Mvc/

## Bootstrapping
In Application_Start in Global.asax.cs add the following lines:

```C#
  // In case you want to remove the other view engines, do this:
  ViewEngines.Engines.Clear();
  
  // Create the view engine.
  var hbsve = new HandlebarsViewEngine();
  
  // The builtin helpers aren't added by default - you need to opt-in to have them available.
  hbsve.RegisterMvcHelpers();
  hbsve.RegisterSectionsHelpers();
  
  // Add the Handlebars view engine
  ViewEngines.Engines.Add(hbsve);
```

## Example

#### controller
```C#
public class HomeController : Controller
{
  public ActionResult Index()
  {
    var model = new
    {
      first = "John",
      last  = "Doe"
    }
    return View(model);
  }
}
```

#### View file (~/Views/Home/Index.hbs)

```
{{!< default}}
Hello, {{first}} {{last}}!
```

#### Layout file (~/Views/_Layouts/default.hbs)

```HTML
<html>
<body>
{{{body}}}
</body>
</html>
```

#### Renders:

```HTML
<html>
<body>
Hello, John Doe!
</body>
</html>
