﻿/*
Copyright (c) 2016, Greg Diaz
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

using MatterHackers.Agg.UI;
using MatterHackers.MatterControl.CustomWidgets;

namespace MatterHackers.MatterControl.PrinterControls.PrinterConnections
{
	public class ShowAuthPanel : SetupConnectionWidgetBase
	{
		public ShowAuthPanel(WizardWindow connectionWizard) : base(connectionWizard)
		{

			TextWidget userLoginPromptLabel = new TextWidget("Would you like to sign in to access your cloud\nprinter profiles?")
			{
				PointSize = 12,
				TextColor = ActiveTheme.Instance.PrimaryTextColor,
			};
			contentRow.AddChild(userLoginPromptLabel);

			var nextButton = textImageButtonFactory.Generate("Skip");
			nextButton.Name = "Connection Wizard Skip Sign In Button";
			nextButton.Click += (sender, e) =>
			{
				connectionWizard.ChangeToAddPrinter();
			};

			var signInButton = textImageButtonFactory.Generate("Sign In");
			signInButton.Name = "Sign In From Connection Wizard Button";
			signInButton.Click += (s, e) =>
			{ 
				WizardWindow.ShowAuthDialog?.Invoke();
				UiThread.RunOnIdle(connectionWizard.Close);
	
			};

			footerRow.AddChild(nextButton);
			footerRow.AddChild(new HorizontalSpacer());
			footerRow.AddChild(signInButton);
		}
	}
}