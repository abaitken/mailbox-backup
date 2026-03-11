using System;
using System.Collections.Generic;
using MailboxBackup.Clients;

namespace MailboxBackup
{
    abstract class LocalOrganisationStrategy
    {
        public abstract string Apply(IMailItem message, IRemoteFolder folder);

        public static LocalOrganisationStrategy Create(string strategyName, string outputDir)
        {
            return strategyName switch
            {
                "FollowFolderStructure" => new FollowFolderStructureOrganisationStrategy(outputDir),
                "DatedFolderStructure" => new DatedFolderStructureOrganisationStrategy(outputDir),
                _ => throw new ArgumentOutOfRangeException(nameof(strategyName)),
            };
        }

        public static IEnumerable<string> Strategies
        {
            get => new[] { "FollowFolderStructure", "DatedFolderStructure" };
        }
    }
}