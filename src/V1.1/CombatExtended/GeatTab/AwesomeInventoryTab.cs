﻿// <copyright file="AwesomeInventoryTab.cs" company="Zizhen Li">
// Copyright (c) 2019 - 2020 Zizhen Li. All rights reserved.
// Licensed under the LGPL-3.0-only license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwesomeInventory.UI
{
    /// <inheritdoc/>
    public class AwesomeInventoryTab : AwesomeInventoryTabBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AwesomeInventoryTab"/> class.
        /// </summary>
        public AwesomeInventoryTab()
        {
            _drawGearTab = new CEDrawGearTabWorker(this);
        }
    }
}