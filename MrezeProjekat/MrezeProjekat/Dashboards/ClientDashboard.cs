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

        private readonly List<string> _clients  = new List<string>();
        private readonly List<string> _servers  = new List<string>();
        private readonly List<string> _networks = new List<string>();
        private readonly List<string> _inputs   = new List<string>();
        private readonly List<Table> _nodes     = new List<Table>();

        protected readonly int maxLogsBiger = 15;
        protected readonly int maxLogsSmall = 5;
        private bool _update = false;

        // The Dashboard constructor defines the
        // structural layout of the client console
        public ClientDashboard()
        {
            this._root = new Layout("Root")
                .SplitRows(
                    new Layout("TopRow").SplitColumns(
                       this. _clientInfo = new Layout("ClientInfo"),
                        new Layout("RightTop").SplitRows(
                           this._input = new Layout("Input"),
                            this._serverInfo = new Layout("ServerInfo")
                        )
                    ),
                    this._nodeInfo = new Layout("NodeInfo"),
                    this._networkInfo = new Layout("NetworkInfo"),
                    this._consoleInput = new Layout("ConsoleInput").Size(1)
                );

            this._clientInfo.Update(new Panel("").Header("[orange1]Client-Info[/]").Expand());
            this._input.Update(new Panel("").Header("[blue]Input[/]").Expand());
            this._serverInfo.Update(new Panel("").Header("[cyan]Server-Info[/]").Expand());
            this._networkInfo.Update(new Panel("").Header("[lime]Network-Info[/]").Expand());
            this._nodeInfo.Update(new Panel("").Header("[bold]Node-Info[/]").Expand());
            this._consoleInput.Update(new Markup("[grey]Type your message here >> [/]"));
        }

        public override void AddClient(string text) { _clients.Add(text); _update = true; }
        public override void AddServer(string text) { _servers.Add(text); _update = true; }
        public override void AddNetwork(string text) { _networks.Add(text); _update = true; }
        public void AddInput(string text) { _inputs.Add(text); _update = true; }
        public void AddNode(Table table) { _nodes.Add(table); _update = true; }

        public bool NeedsUpdate() { return _update; }

        public override void RefreshPanels()
        {

            if (this._clients.Count >= maxLogsBiger) this._clients.RemoveAt(0);
            this._clientInfo.Update(new Panel(new Markup(string.Join("\n", this._clients)))
                .Header("[orange1]Client-Info[/]").Expand());

            if (this._servers.Count >= maxLogsSmall) this._servers.RemoveAt(0);
            this._serverInfo.Update(new Panel(new Markup(string.Join("\n",this. _servers)))
                .Header("[cyan]Server-Info[/]").Expand());

            if (this._inputs.Count >= maxLogsSmall) this._inputs.RemoveAt(0);
            this._input.Update(new Panel(new Markup(string.Join("\n", this._inputs)))
                .Header("[blue]Input[/]").Expand());

            if (this._networks.Count >= maxLogsBiger) this._networks.RemoveAt(0);
           this. _networkInfo.Update(new Panel(new Markup(string.Join("\n", this._networks)))
                .Header("[lime]Network-Info[/]").Expand());

            if (this._nodes.Count >= maxLogsBiger) this._nodes.RemoveAt(0);
            this._nodeInfo.Update(this._nodes.Count > 0
                ? new Panel(new Columns(_nodes.ToArray())).Header("[bold]Node-Info[/]").Expand()
                : new Panel("No nodes yet").Header("[bold]Node-Info[/]").Expand());

            this._update = false;
            
        }

    }
}
