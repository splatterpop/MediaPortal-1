#region Copyright (C) 2005-2009 Team MediaPortal

/*
 *  Copyright (C) 2005-2009 Team MediaPortal
 *  http://www.team-mediaportal.com
 *  
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *  
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *  
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.IO;
using MediaPortal.Utilities.CommandLine;

namespace UninstallFilelist
{
  internal class Program
  {
    private static void Main(string[] args)
    {
      ICommandLineOptions argsOptions = new CommandLineOptions();

      try
      {
        CommandLine.Parse(args, ref argsOptions);
      }
      catch (ArgumentException)
      {
        argsOptions.DisplayOptions();
        Environment.Exit(0);
      }

      CommandLineOptions options = argsOptions as CommandLineOptions;

      if (!options.IsOption(CommandLineOptions.Option.dir)
          || !options.IsOption(CommandLineOptions.Option.output)
        )
      {
        argsOptions.DisplayOptions();
        Environment.Exit(0);
      }
      string directory = options.GetOption(CommandLineOptions.Option.dir);
      string output = options.GetOption(CommandLineOptions.Option.output);

      string ignore = string.Empty;
      if (options.IsOption(CommandLineOptions.Option.ignore))
      {
        TextReader reader = new StreamReader(options.GetOption(CommandLineOptions.Option.ignore));
        ignore = reader.ReadToEnd();
      }


      FileLister lister = new FileLister(directory, ignore);
      lister.UpdateAll();

      if (File.Exists(output))
      {
        File.Delete(output);
      }

      TextWriter write = new StreamWriter(output, false, System.Text.Encoding.Default);
      write.Write(lister.FileList);
      write.Close();
    }
  }
}