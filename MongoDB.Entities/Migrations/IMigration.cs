using System;
using System.Collections.Generic;
using System.Text;

namespace MongoDB.Entities
{
    public interface IMigration
    {
        void Upgrade();
    }
}
