using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pkuyo.Wanderer.Creatures
{
    public class BabyLizard : Creature

    {
        public BabyLizard(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
        {
        }

        public int foodInStomach;

        public int MaxFood = 5;
    }
    
}
