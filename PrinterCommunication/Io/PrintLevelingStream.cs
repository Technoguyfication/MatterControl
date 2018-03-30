﻿/*
Copyright (c) 2014, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;
using MatterControl.Printing;
using MatterHackers.MatterControl.ConfigurationPage.PrintLeveling;
using MatterHackers.MatterControl.SlicerConfiguration;

namespace MatterHackers.MatterControl.PrinterCommunication.Io
{
	public class PrintLevelingStream : GCodeStreamProxy
	{
		PrinterSettings printerSettings;
		bool activePrinting;
		protected PrinterMove lastDestination = new PrinterMove();
		public PrintLevelingStream(PrinterSettings printerSettings, GCodeStream internalStream, bool activePrinting)
			: base(internalStream)
		{
			this.printerSettings = printerSettings;
			this.activePrinting = activePrinting;
		}

		public PrinterMove LastDestination { get { return lastDestination; } }

		public static bool Enabled { get; set; } = true;

		public override string ReadLine()
		{
			string lineFromChild = base.ReadLine();

			if (lineFromChild != null
				&& PrintLevelingStream.Enabled
				&& printerSettings.GetValue<bool>(SettingsKey.print_leveling_enabled)
				&& !printerSettings.GetValue<bool>(SettingsKey.has_hardware_leveling))
			{
				if (LineIsMovement(lineFromChild))
				{
					PrinterMove currentDestination = GetPosition(lineFromChild, lastDestination);
					lineFromChild = RunPrintLevelingTranslations(lineFromChild, currentDestination);
					lastDestination = currentDestination;

					return lineFromChild;
				}
				else if (lineFromChild.StartsWith("G29"))
				{
					// remove G29 (machine prob bed) if we are running our own leveling.
					lineFromChild = base.ReadLine(); // get the next line instead
				}
			}

			return lineFromChild;
		}

		public override void SetPrinterPosition(PrinterMove position)
		{
			string lineBeingSent = CreateMovementLine(position);
			string leveledPosition = RunPrintLevelingTranslations(lineBeingSent, position);

			PrinterMove leveledDestination = GetPosition(leveledPosition, PrinterMove.Nowhere);
			PrinterMove deltaToLeveledPosition = leveledDestination - position;

			PrinterMove withoutLevelingOffset = position - deltaToLeveledPosition;

			lastDestination = withoutLevelingOffset;
			lastDestination.extrusion = position.extrusion;
			lastDestination.feedRate = position.feedRate;

			internalStream.SetPrinterPosition(lastDestination);
		}

		private string RunPrintLevelingTranslations(string lineBeingSent, PrinterMove currentDestination)
		{
			PrintLevelingData levelingData = printerSettings.Helpers.GetPrintLevelingData();
			if (levelingData != null
				&& printerSettings?.GetValue<bool>(SettingsKey.print_leveling_enabled) == true
				&& (lineBeingSent.StartsWith("G0 ") || lineBeingSent.StartsWith("G1 ")))
			{
				lineBeingSent = LevelWizardBase.GetLevelingFunctions(printerSettings, 3, 3, levelingData)
					.DoApplyLeveling(lineBeingSent, currentDestination.position);
			}

			return lineBeingSent;
		}
	}
}