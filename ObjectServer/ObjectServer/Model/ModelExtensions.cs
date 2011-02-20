﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectServer.Model
{
    public static class ModelExtensions
    {
        public static bool ContainsField(this IModel model, string fieldName)
        {
            return model.DefinedFields.Count(f => f.Name == fieldName) > 0;
        }

        public static IEnumerable<IField> GetAllStorableFields(this IModel model)
        {
            return model.DefinedFields.Where(f => f.IsStorable() && f.Name != "id");
        }
    }
}
