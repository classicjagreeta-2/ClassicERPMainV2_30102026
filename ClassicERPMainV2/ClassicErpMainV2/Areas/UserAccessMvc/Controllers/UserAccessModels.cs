using System;
using System.Collections.Generic;

namespace ClassicErpMainV2.Areas.UserAccessMvc.Models
{
    // Maps VBMENU_FIN — the master menu tree the rights are granted against.
    public class MenuNode
    {
        public string Id { get; set; }          // was Rs1!ID
        public string ParentId { get; set; }     // was Rs1!PID
        public string IndexOra { get; set; }      // was Rs1!INDEX_ORA
        public string Name { get; set; }
        public string Type { get; set; }          // "E" / "E1" = leaf w/ Add/Edit/Delete/View children, "S" = section
        public bool DtLock { get; set; }          // was Rs1!dtlock = "Y"
        public int Image { get; set; }

        public List<MenuNode> Children { get; set; } = new List<MenuNode>();

        // Composite key used in the old tree: "ID!INDEX_ORA" (matches TreeView1 node keys)
        public string Key => string.IsNullOrEmpty(IndexOra) ? Id : $"{Id}!{IndexOra}";
    }

    // One granted right against one menu node, for either a user or a group.
    // Mirrors a row in USRACS_FIN / GRPACS_FIN.
    public class MenuRight
    {
        public string MnuId { get; set; }
        public string IndexOra { get; set; }
        /// <summary>Any combination of 'A','E','D','V', or "S" for a section-only grant.</summary>
        public string Aedv { get; set; } = "";
        public int AddLockDays { get; set; }
        public int EditLockDays { get; set; }
        public int DeleteLockDays { get; set; }
    }

    public class VoucherTypeRight
    {
        public string Type { get; set; }
        public string DescOra { get; set; }
        public bool Selected { get; set; }
    }

    public class UnitLink
    {
        public string UnitCode { get; set; }
        public string UnitName { get; set; }
        public bool IsAssigned { get; set; }
    }

    // Was Text1/Text2/Text3/Text5/user_name/Txt* fields + USRACS_FIN/USER_JAG rows combined.
    public class UserAccessRecord
    {
        public string UserOra { get; set; }        // login id (was Text1)
        public string UserName { get; set; }        // display name
        public string Group { get; set; }            // was Text5 (group a user optionally belongs to)
        public string Password { get; set; }         // plaintext in transit only — hashed/encoded before storage
        public string PasswordConfirm { get; set; }
        public string EmailAddress { get; set; }
        public string SmtpServer { get; set; }
        public string SmtpPort { get; set; }
        public string SmtpPassword { get; set; }

        public bool IsNewRecord { get; set; }         // FRMMODE = "A"
        public bool IsEditingPassword { get; set; }    // Text2.Tag = 3 equivalent

        public List<MenuRight> Rights { get; set; } = new List<MenuRight>();
        public List<UnitLink> AssignedUnits { get; set; } = new List<UnitLink>();
        public List<VoucherTypeRight> VoucherRights { get; set; } = new List<VoucherTypeRight>();
    }

    public class GroupAccessRecord
    {
        public string GroupId { get; set; }           // was Text4, max 8 chars in the legacy form
        public List<MenuRight> Rights { get; set; } = new List<MenuRight>();
    }
    // One row of the "User Rights Listing" grid. Not a legacy VB6 screen — the original
    // form only ever showed one user at a time via Text1/RSUSR navigation — so this is a
    // new summary view assembled from USER_JAG + USRACS_FIN + GRPACS_FIN.
    public class UserListItem
    {
        public string UserOra { get; set; }
        public string UserName { get; set; }
        public string GroupId { get; set; }
        public string GroupName { get; set; }   // resolution depends on how your GRPACS_FIN/group-label table is set up — see repository note
        public string Email { get; set; }
        public bool IsActive { get; set; }       // assumes an IS_ACTIVE column on USER_JAG; the legacy schema shown had no status flag
        public int RightsGranted { get; set; }
        public int RightsTotal { get; set; }
    }
}
