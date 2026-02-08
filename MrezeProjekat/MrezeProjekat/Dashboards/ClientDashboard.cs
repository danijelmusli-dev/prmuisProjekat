using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading; 
using System.Threading.Tasks;

using MrezeProjekat.Dashboards;

namespace MrezeProjekat.Helpers
{
    public class ClientDashboard : Dashboard
    {

        private readonly Layout _clientInfo;
        private readonly Layout _serverInfo;
        private readonly Layout _networkInfo;
        private readonly Layout _input;
        private readonly Layout _nodeInfo;
        private readonly Layout _consoleInput;

        private readonly List<string> _clients = new List<string>();
        private readonly List<string> _servers = new List<string>();
        private readonly List<string> _networks = new List<string>();
        private readonly List<string> _inputs = new List<string>();
        private readonly List<Table> _nodes = new List<Table>();

        protected readonly int maxLogsBiger = 15;
        protected readonly int maxLogsSmall = 5;
        private string _typed = string.Empty;

        public ClientDashboard()
        {
            _root = new Layout("Root")
                .SplitRows(
                    new Layout("TopRow").SplitColumns(
                        _clientInfo = new Layout("ClientInfo"),
                        new Layout("RightTop").SplitRows(
                            _input = new Layout("Input"),
                            _serverInfo = new Layout("ServerInfo")
                        )
                    ),
                    _nodeInfo = new Layout("NodeInfo"),
                    _networkInfo = new Layout("NetworkInfo"),
                    _consoleInput = new Layout("ConsoleInput").Size(1)
                );

            _clientInfo.Update(new Panel("").Header("[orange1]Client-Info[/]").Expand());
            _input.Update(new Panel("").Header("[blue]Input[/]").Expand());
            _serverInfo.Update(new Panel("").Header("[cyan]Server-Info[/]").Expand());
            _networkInfo.Update(new Panel("").Header("[lime]Network-Info[/]").Expand());
            _nodeInfo.Update(new Panel("").Header("[bold]Node-Info[/]").Expand());
            _consoleInput.Update(new Markup("[grey]Type your message here >> [/]"));
        }

        public override void AddClient(string text) => _clients.Add(text);
        public override void AddServer(string text) => _servers.Add(text);
        public override void AddNetwork(string text) => _networks.Add(text);
        public void AddInput(string text) => _inputs.Add(text);
        public void AddNode(Table table) => _nodes.Add(table);

        public override void RefreshPanels()
        {
            if (_clients.Count >= maxLogsBiger) _clients.RemoveAt(0);
            _clientInfo.Update(new Panel(new Markup(string.Join("\n", _clients)))
                .Header("[orange1]Client-Info[/]").Expand());

            if (_servers.Count >= maxLogsSmall) _servers.RemoveAt(0);
            _serverInfo.Update(new Panel(new Markup(string.Join("\n", _servers)))
                .Header("[cyan]Server-Info[/]").Expand());

            if (_inputs.Count >= maxLogsSmall) _inputs.RemoveAt(0);
            _input.Update(new Panel(new Markup(string.Join("\n", _inputs)))
                .Header("[blue]Input[/]").Expand());

            if (_networks.Count >= maxLogsBiger) _networks.RemoveAt(0);
            _networkInfo.Update(new Panel(new Markup(string.Join("\n", _networks)))
                .Header("[lime]Network-Info[/]").Expand());

            if (_nodes.Count >= maxLogsBiger) _nodes.RemoveAt(0);
            _nodeInfo.Update(_nodes.Count > 0
                ? new Panel(new Columns(_nodes.ToArray())).Header("[bold]Node-Info[/]").Expand()
                : new Panel("No nodes yet").Header("[bold]Node-Info[/]").Expand());

            _consoleInput.Update(new Markup($"[grey]Type your message here >> {_typed}[/]"));
        }


    }
}
