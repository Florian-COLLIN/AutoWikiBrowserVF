/*
Autowikibrowser
(C) 2007 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AutoWikiBrowser
{
    internal sealed partial class NudgeTimer : System.Windows.Forms.Timer
    {
        /* TODO: I'm quite certain the logic isn't right here. The timer needs to be started and reset on a
         * successful save, and it needs to increase the time until next fire if the page still doesn't get
         * saved (e.g. wiki or net connection is down). */
        // Events
        public new event TickEventHandler Tick;
        public delegate void TickEventHandler(object sender, NudgeTimer.NudgeTimerEventArgs EventArgs);

        // Methods
        public NudgeTimer(IContainer container)
            : base(container)
        {
            base.Tick += new EventHandler(this.NudgeTimer_Tick);
        }

        public void StartMe()
        {
            //base.Interval = 120000;
            base.Start();
        }

        public void Reset()
        {
            base.Interval = 120000;
        }

        private void NudgeTimer_Tick(object sender, EventArgs eventArgs)
        {
            NudgeTimerEventArgs myEventArgs = new NudgeTimerEventArgs();
            Tick(this, myEventArgs);
            if (!myEventArgs.Cancel)
            {
                switch (base.Interval)
                {
                    case 120000:
                        base.Interval = 240000;
                        break;

                    case 240000:
                        base.Interval = 360000;
                        break;

                    case 360000:
                        base.Interval = 600000;
                        break;
                }
            }
        }

        // Properties
        public override bool Enabled
        {
            get { return base.Enabled; }
            set
            {
                base.Enabled = value;
                base.Interval = 120000;
            }
        }

        // Nested Types
        internal sealed class NudgeTimerEventArgs : EventArgs
        {
            // Fields
            private bool mCancel;

            // Properties
            public bool Cancel
            {
                get { return this.mCancel; }
                set { this.mCancel = value; }
            }
        }
    }
}
