using System;

namespace Agromancy.APIs;

public interface ISpaceCoreApi
    {
        /// Must have [XmlType("Mods_SOMETHINGHERE")] attribute (required to start with "Mods_")
        void RegisterSerializerType(Type type);
    }