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

        public static int GetIntegerInput(int min, int max, string name)
        {
            int number;
            do
            {
                number = AnsiConsole.Ask<int>($"Enter [lightgreen]{name}[/] (between [red]{min}[/] and [red]{max}[/]):");
                if (number < min || number > max)
                {
                    AnsiConsole.MarkupLine("[red]Invalid input![/]");
                }
            } while (number < min || number > max);

            return number;
        }

        public static void OnionLogo()
        {
            AnsiConsole.MarkupLine("\t\t\t\t\t\t[white]   ____        _           [/]");
            AnsiConsole.MarkupLine("\t\t\t\t\t\t[#CCFFCC]  ╱ __ ╲____  (_)___  ____ [/]");
            AnsiConsole.MarkupLine("\t\t\t\t\t\t[#66FF66] ╱ ╱ ╱ ╱ __ ╲╱ ╱ __ ╲╱ __ ╲[/]");
            AnsiConsole.MarkupLine("\t\t\t\t\t\t[green]╱ ╱_╱ ╱ ╱ ╱ ╱ ╱ ╱_╱ ╱ ╱ ╱ ╱[/]");
            AnsiConsole.MarkupLine("\t\t\t\t\t\t[darkgreen]╲____╱_╱_╱_╱_╱╲____╱_╱ ╱_╱ [/]");
            AnsiConsole.MarkupLine("\t\t\t\t\t\t[#FFE4E1]    __  __          __       [/]");
            AnsiConsole.MarkupLine("\t\t\t\t\t\t[#FFE4E1]   ╱ │ ╱ ╱___  ____╱ ╱__    [/]");
            AnsiConsole.MarkupLine("\t\t\t\t\t\t[magenta]  ╱  │╱ ╱ __ ╲╱ __  ╱ _ ╲   [/]");
            AnsiConsole.MarkupLine("\t\t\t\t\t\t[#8B008B] ╱ ╱│  ╱ ╱_╱ ╱ ╱_╱ ╱  __╱   [/]");
            AnsiConsole.MarkupLine("\t\t\t\t\t\t[purple]╱_╱ │_╱╲____╱╲____╱╲___╱    [/]");
            AnsiConsole.MarkupLine("                            ");
        }

        public static void PrintInstructions(Instructions ins)
        {
            AnsiConsole.MarkupLineInterpolated($"\t[[[MediumSpringGreen]INSTRUCTIONS[/]]] | [Gold1]Prev Node[/] {ins.PrevNode} | [DeepSkyBlue1]Next Node[/] {ins.NextNode} | [[[MediumSpringGreen]INSTRUCTIONS[/]]]");
        }

        public static void PrintRequest(Request req)
        {
            AnsiConsole.MarkupLineInterpolated($"\t[[[DarkViolet_1]REQUEST[/]]] | {req.Sender} | Node {req.NodeNum} | Max {req.MaxMessages} | [[[DarkViolet_1]REQUEST[/]]]");
        }

    }
}
    


