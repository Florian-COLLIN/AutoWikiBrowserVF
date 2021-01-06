﻿/*
Autowikibrowser
Copyright (C) 2007 Martin Richards

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Windows.Forms;

namespace AutoWikiBrowser
{
    internal sealed partial class ExitQuestion : Form
    {
        public ExitQuestion(TimeSpan time, int edits, string msg)
        {
            InitializeComponent();

            lblPrompt.Text = msg + "Are you sure you want to exit?";

            lblTimeAndEdits.Text = string.Format("You made {0} edits in {1}", edits, time);
        }

        public bool CheckBoxDontAskAgain
        {
            get { return chkDontAskAgain.Checked; }
        }
    }
}
