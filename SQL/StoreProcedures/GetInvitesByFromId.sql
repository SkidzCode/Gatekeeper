-- =========================================
-- 2. GetInvitesByFromId
--    Brings back status info like:
--      - Whether Verification has expired,
--      - Whether it's been revoked or completed,
--      - Whether the Notification has been sent.
-- =========================================
DROP PROCEDURE IF EXISTS GetInvitesByFromId;
DELIMITER $$
CREATE PROCEDURE GetInvitesByFromId (
    IN p_FromId INT
)
BEGIN
    SELECT 
        i.Id,
        i.FromId,
        i.ToName,
        i.ToEmail,
        i.Created,
        -- Verification status
        (CASE WHEN v.ExpiryDate < NOW() THEN TRUE ELSE FALSE END) AS IsExpired,
        v.Revoked AS IsRevoked,
        v.Complete AS IsComplete,
        -- Notification status
        (CASE WHEN n.IsSent = TRUE THEN TRUE ELSE FALSE END) AS IsSent
    FROM Invites i
    JOIN Verification v ON i.VerificationId = v.Id
    LEFT JOIN Notifications n ON i.NotificationId = n.Id
    WHERE i.FromId = p_FromId;
END$$
DELIMITER ;