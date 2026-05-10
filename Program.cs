using NBATerminal.Controllers;
using NBATerminal.Services;
using NBATerminal.Views;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;

var app = Application.Create();

app.Init();

var mainView = new MainView
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var pythonBridge = new PythonBridge();
var gameService = new GameService(pythonBridge);
var statsService = new StatsService(pythonBridge);

var controller = new MainController(mainView, gameService, statsService, app);
controller.Initialize();

app.Run(mainView);
