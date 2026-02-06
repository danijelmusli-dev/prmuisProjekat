using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MrezeProjekat.Dashboards;

namespace MrezeProjekat.Helpers
{
    public class ServerDashboard : Dashboard
    {

        private readonly Layout _logo;
        private readonly Layout _topRight;
        private readonly Layout _bottomLeft;
        private readonly Layout _bottomRight;

        private readonly List<string> _clientsS = new List<string>();
        private readonly List<string> _serversS = new List<string>();
        private readonly List<string> _inputsS = new List<string>();

        protected readonly int maxLogs = 20;

        public ServerDashboard()
        {
            _root = new Layout("Root")
                .SplitRows(
                    new Layout("TopRow").SplitColumns(
                        _logo = new Layout("Logo"),
                        _topRight = new Layout("TopRight")
                    ),
                    new Layout("BottomRow").SplitColumns(
                        _bottomLeft = new Layout("BottomLeft"),
                        _bottomRight = new Layout("BottomRight")
                    )
                );

            // ubaci ASCII logo u panel
            _logo.Update(
                new Panel(new Markup(this._logoString)).Expand().Border(BoxBorder.None) // možeš da promeniš okvir
            );

            _topRight.Update(new Panel("").Header("[blue]Input[/]").Expand());
            _bottomLeft.Update(new Panel("").Header("[orange1]Client-Info[/]").Expand());
            _bottomRight.Update(new Panel("").Header("[cyan]Server-Info[/]").Expand());
        }

        private string _logoString = @"
        [white]   ____        _           [/]
        [#CCFFCC]  ╱ __ ╲____  (_)___  ____ [/]
        [#66FF66] ╱ ╱ ╱ ╱ __ ╲╱ ╱ __ ╲╱ __ ╲[/]
        [green]╱ ╱_╱ ╱ ╱ ╱ ╱ ╱ ╱_╱ ╱ ╱ ╱ ╱[/]
        [darkgreen]╲____╱_╱_╱_╱_╱╲____╱_╱ ╱_╱ [/]
        [#FFE4E1]    __  __          __       [/]
        [#FFE4E1]   ╱ │ ╱ ╱___  ____╱ ╱__    [/]
        [magenta]  ╱  │╱ ╱ __ ╲╱ __  ╱ _ ╲   [/]
        [#8B008B] ╱ ╱│  ╱ ╱_╱ ╱ ╱_╱ ╱  __╱   [/]
        [purple]╱_╱ │_╱╲____╱╲____╱╲___╱    [/]
        ";


        public override void AddClient(string text) => _clientsS.Add(text);
        public override void AddServer(string text) => _serversS.Add(text);
        public override void AddNetwork(string text) { return; }
        public void AddInput(string text) => _inputsS.Add(text);

        public override void RefreshPanels()
        {
            if (_clientsS.Count >= maxLogs) _clientsS.RemoveAt(0);
            _bottomLeft.Update(new Panel(new Markup(string.Join("\n", _clientsS)))
                .Header("[orange1]Client-Info[/]").Expand());

            if (_serversS.Count >= maxLogs) _serversS.RemoveAt(0);
            _bottomRight.Update(new Panel(new Markup(string.Join("\n", _serversS)))
                .Header("[cyan]Server-Info[/]").Expand());

            if (_inputsS.Count >= maxLogs) _inputsS.RemoveAt(0);
            _topRight.Update(new Panel(new Markup(string.Join("\n", _inputsS)))
                .Header("[blue]Input[/]").Expand());
        }


    }
}
