using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starbot
{
    public class Objective
    {
        //once this returns true, the bot will look for a new objective, or sleep if there aren't any.
        public virtual bool IsComplete { get; set; }

        public Objective()
        {
            IsComplete = false;
        }

        //called whenever the bot is "bored" until IsComplete is true
        public virtual void Step()
        {

        }
    }
}
