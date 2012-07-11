// 
// PointOfSale.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix, and contributors.
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Mictlanix.BE.Model
{
    [ActiveRecord("point_sale")]
    public class PointOfSale : ActiveRecordLinqBase<PointOfSale>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "point_sale_id")]
        public int Id { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Store", ResourceType = typeof(Resources))]
        [UIHint("StoreSelector")]
        public int StoreId { get; set; }
		
        [BelongsTo("store")]
        [Display(Name = "Store", ResourceType = typeof(Resources))]
        public virtual Store Store { get; set; }

		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Warehouse", ResourceType = typeof(Resources))]
        [UIHint("WarehouseSelector")]
        public int WarehouseId { get; set; }
		
        [BelongsTo("warehouse")]
        [Display(Name = "Warehouse", ResourceType = typeof(Resources))]
        public virtual Warehouse Warehouse { get; set; }

        [Property]
		[ValidateIsUnique]
        [Display(Name = "Code", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(25, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Code { get; set; }

        [Property]
        [Display(Name = "Name", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Name { get; set; }

        [Property]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Comment", ResourceType = typeof(Resources))]
        [StringLength(500, MinimumLength = 0)]
        public string Comment { get; set; }

        #region Override Base Methods

        public override string ToString()
        {
            return string.Format("{1} [{0}, {2}]", Code, Name, Store);
        }

        public override bool Equals(object obj)
        {
            PointOfSale other = obj as PointOfSale;

            if (other == null)
                return false;

            if (Id == 0 && other.Id == 0)
                return (object)this == other;
            else
                return Id == other.Id;
        }

        public override int GetHashCode()
        {
            if (Id == 0)
                return base.GetHashCode();

            return string.Format("{0}#{1}", GetType().FullName, Id).GetHashCode();
        }

        #endregion
    }
}