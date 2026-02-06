using MrezeProjekat.Models;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;



namespace MrezeProjekat.Helpers
{
    public static class Menu
    {
        // Foreground color (tekst u boji)
        public static void ColorFG(string text, Color foregroundColor)
        {
            var styled = new Text(text, new Style(foregroundColor));
            AnsiConsole.Write(styled);
            AnsiConsole.WriteLine();
        }

        // Background color (tekst na obojenoj pozadini)
        public static void ColorBG(string text, Color backgroundColor)
        {
            var styled = new Text(text, new Style(Color.Black, backgroundColor));
            AnsiConsole.Write(styled);
            AnsiConsole.WriteLine();
        }

        // Foreground + Background zajedno
        public static void ColorFBG(string text, Color foregroundColor, Color backgroundColor)
        {
            var styled = new Text(text, new Style(foregroundColor, backgroundColor));
            AnsiConsole.Write(styled);
            AnsiConsole.WriteLine();
        }


        public static void OnionLogo()
        {
            // ASCII logo obojen liniju po liniju
            AnsiConsole.MarkupLine("[white]   ____        _           [/]");
            AnsiConsole.MarkupLine("[#CCFFCC]  ╱ __ ╲____  (_)___  ____ [/]");
            AnsiConsole.MarkupLine("[#66FF66] ╱ ╱ ╱ ╱ __ ╲╱ ╱ __ ╲╱ __ ╲[/]");
            AnsiConsole.MarkupLine("[green]╱ ╱_╱ ╱ ╱ ╱ ╱ ╱ ╱_╱ ╱ ╱ ╱ ╱[/]");
            AnsiConsole.MarkupLine("[darkgreen]╲____╱_╱_╱_╱_╱╲____╱_╱ ╱_╱ [/]");
            AnsiConsole.MarkupLine("[#FFE4E1]    __  __          __       [/]");
            AnsiConsole.MarkupLine("[#FFE4E1]   ╱ │ ╱ ╱___  ____╱ ╱__    [/]");
            AnsiConsole.MarkupLine("[magenta]  ╱  │╱ ╱ __ ╲╱ __  ╱ _ ╲   [/]");
            AnsiConsole.MarkupLine("[#8B008B] ╱ ╱│  ╱ ╱_╱ ╱ ╱_╱ ╱  __╱   [/]");
            AnsiConsole.MarkupLine("[purple]╱_╱ │_╱╲____╱╲____╱╲___╱    [/]");
            AnsiConsole.MarkupLine("                            ");
        }

        public static void PrintInstructions(ClientDashboard dash, Instructions ins)
        {
            var table = new Table();
            table.Expand = false; // ne širi se preko celog ekrana
            table.Border = TableBorder.Minimal; // kompaktniji border

            table.AddColumn(new TableColumn("[silver]PrevNode[/]").Centered());
            table.AddColumn(new TableColumn("[skyblue1]NextNode[/]").Centered());

            table.AddRow(ins.PrevNode?.ToString() ?? "[orange1]Client[/]", ins.NextNode?.ToString() ?? "[dodgerblue1]Server[/]");

            // opcionalno: postavi širinu kolona
            table.Columns[0].Width = 10;
            table.Columns[1].Width = 10;

            dash.AddNode(table);

        }

        public static void PrintRequest(ClientDashboard dash, Request req)
        {
            // umesto tabele, samo mali tekst
            var text = $"Request from {req.Sender} | Node {req.NodeNum} | Max {req.MaxMessages}";
            dash.AddClient(text);
        }

    }
}
    


