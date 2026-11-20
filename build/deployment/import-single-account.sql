-- Run with the fresh and migrated IgelPortalNew database selected.
-- Set @igel_import_email in the same MariaDB session before sourcing this file.
DELIMITER //
BEGIN NOT ATOMIC
    DECLARE imported_user_id VARCHAR(255);
    DECLARE matching_users INT;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF NOT (DATABASE() <=> 'IgelPortalNew') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Select the IgelPortalNew destination database.';
    END IF;
    IF @igel_import_email IS NULL OR TRIM(@igel_import_email) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Set @igel_import_email before importing.';
    END IF;

    START TRANSACTION WITH CONSISTENT SNAPSHOT;
    IF EXISTS (SELECT 1 FROM AspNetUsers) OR EXISTS (SELECT 1 FROM Hedgehogs)
        OR EXISTS (SELECT 1 FROM Territories) OR EXISTS (SELECT 1 FROM TerritoryServices)
        OR EXISTS (SELECT 1 FROM TerritoryAttachedServices) OR EXISTS (SELECT 1 FROM Communities)
        OR EXISTS (SELECT 1 FROM CommunityMembers) OR EXISTS (SELECT 1 FROM ServiceRequests)
        OR EXISTS (SELECT 1 FROM ServiceRequestResponses) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Destination already contains accounts or portal data.';
    END IF;

    SELECT COUNT(*), MIN(Id) INTO matching_users, imported_user_id
    FROM IgelPortal.AspNetUsers
    WHERE NormalizedEmail = UPPER(TRIM(@igel_import_email));

    IF matching_users <> 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Expected exactly one source account with this email.';
    END IF;

    -- Replace freshly seeded role definitions with the source definitions and permissions.
    DELETE FROM AspNetRoleClaims;
    DELETE FROM AspNetRoles;
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        SELECT Id, Name, NormalizedName, ConcurrencyStamp FROM IgelPortal.AspNetRoles;
    INSERT INTO AspNetRoleClaims (RoleId, ClaimType, ClaimValue)
        SELECT RoleId, ClaimType, ClaimValue FROM IgelPortal.AspNetRoleClaims;

    INSERT INTO AspNetUsers
        (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash,
         SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
         LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName)
    SELECT Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash,
           UUID(), UUID(), PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
           LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName
    FROM IgelPortal.AspNetUsers WHERE Id = imported_user_id;

    INSERT INTO AspNetUserRoles (UserId, RoleId)
        SELECT UserId, RoleId FROM IgelPortal.AspNetUserRoles WHERE UserId = imported_user_id;
    INSERT INTO AspNetUserClaims (UserId, ClaimType, ClaimValue)
        SELECT UserId, ClaimType, ClaimValue FROM IgelPortal.AspNetUserClaims WHERE UserId = imported_user_id;
    INSERT INTO AspNetUserLogins (LoginProvider, ProviderKey, ProviderDisplayName, UserId)
        SELECT LoginProvider, ProviderKey, ProviderDisplayName, UserId
        FROM IgelPortal.AspNetUserLogins WHERE UserId = imported_user_id;
    INSERT INTO AspNetUserTokens (UserId, LoginProvider, Name, Value)
        SELECT UserId, LoginProvider, Name, Value
        FROM IgelPortal.AspNetUserTokens WHERE UserId = imported_user_id
          AND LoginProvider = '[AspNetUserStore]' AND Name IN ('AuthenticatorKey', 'RecoveryCodes');

    COMMIT;

    SELECT (SELECT COUNT(*) FROM AspNetUsers) AS Accounts,
           (SELECT COUNT(*) FROM AspNetRoles) AS Roles,
           (SELECT COUNT(*) FROM AspNetUserRoles) AS RoleAssignments;
END//
DELIMITER ;
