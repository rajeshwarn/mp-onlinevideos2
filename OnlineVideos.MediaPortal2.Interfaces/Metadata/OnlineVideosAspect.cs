#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace OnlineVideos.MediaPortal2.Interfaces.Metadata
{
    /// <summary>
    /// Contains the metadata specification of media items that are provided by the OnlineVideos plugin.
    /// </summary>
    public class OnlineVideosAspect
    {
        /// <summary>
        /// Media item aspect id of the recording aspect.
        /// </summary>
        public static readonly Guid ASPECT_ID = new Guid("02D73EC4-6689-467A-9CBE-EA70549F5CB4");

        /// <summary>
        /// Name of the SiteUtil which provides the information and will be used for playback.
        /// </summary>
        public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SITEUTIL =
            MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("SiteUtil", 100, Cardinality.Inline, true);

        /// <summary>
        /// Contains an alternative, longer url to the online video. This is needed sometimes, if the url is longer
        /// than <see cref="ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH"/>.
        /// </summary>
        public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_LONGURL =
            MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("LongUrl", 4000, Cardinality.Inline, true);

        /// <summary>
        /// Contains an URL to a poster / cover for the item.
        /// </summary>
        public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_POSTER =
            MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Poster", 200, Cardinality.Inline, true);

        /// <summary>
        /// Contains an URL to a fanart for the item.
        /// </summary>
        public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_FANART =
            MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Fanart", 200, Cardinality.Inline, true);

        public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
            ASPECT_ID, "OnlineVideoItem", new[] {
            ATTR_SITEUTIL,
            ATTR_LONGURL,
            ATTR_POSTER,
            ATTR_FANART,
        });
    }
}
