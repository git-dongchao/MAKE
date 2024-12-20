﻿using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace MAKE
{
    internal sealed class DynamicItemMenuCommand : OleMenuCommand
    {
        public const string guidMAKEPackageCmdSet = "d04c3aba-8f9f-469a-86f8-4f071d420f9b";  // get the GUID from the .vsct file
        public const uint ID_CUSTOM_TEST = 0x8004;
        public String Data;

        private Predicate<int> matches;
        private int rootItemId = 0;

        public DynamicItemMenuCommand(CommandID rootId, Predicate<int> matches, EventHandler invokeHandler, EventHandler beforeQueryStatusHandler)
            : base(invokeHandler, null, beforeQueryStatusHandler, rootId)
        {
            if (matches == null)
            {
                throw new ArgumentNullException("matches");
            }

            this.matches = matches;
        }

        public override bool DynamicItemMatch(int cmdId)
        {
            // Call the supplied predicate to test whether the given cmdId is a match.
            // If it is, store the command id in MatchedCommandid
            // for use by any BeforeQueryStatus handlers, and then return that it is a match.
            // Otherwise clear any previously stored matched cmdId and return that it is not a match.
            if (this.matches(cmdId))
            {
                this.MatchedCommandId = cmdId;
                return true;
            }

            this.MatchedCommandId = 0;
            return false;
        }
    }
}
