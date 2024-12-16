using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static ResoniteWikiMine.Utility.ComponentBatchUpdater;

namespace ResoniteWikiMine.Commands
{
    internal class GetComponentsNotInCategory : ICommand
    {

        private static BatchUpdatePageResult? IsInCategory(Type type, string[] category, string content)
        {

            bool eligible = true;
            for(int i = 0; i < (int)(category.Length / 2); i++)
            {
                if (content.Contains("[[Category:" + category[(i * 2)]))
                {
                    eligible = false;
                }
            }
            for (int i = 0; i < (int) (category.Length / 2); i++)
            {
                if (!content.Contains("[[Category:" + category[(i * 2) + 1]))
                {
                    eligible = false;
                }
            }

            if (eligible)
            {
                Console.WriteLine("https://wiki.resonite.com/Component:" + type.Name);
            }


            return null;
        }

        public async Task<int> Run(WorkContext context, string[] args)
        {
            return UpdateComponentPages(
                context,
                _ => true,
                page => GetComponentsNotInCategory.IsInCategory(page.Type, args, page.Content));
        }
    }
}
