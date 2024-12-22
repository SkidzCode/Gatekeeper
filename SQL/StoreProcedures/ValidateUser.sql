-- Drop the procedure if it exists
DROP PROCEDURE IF EXISTS ValidateUser;

-- Create a stored procedure to validate a refresh token
DELIMITER //
CREATE PROCEDURE ValidateUser(
    IN p_Id VARCHAR(36)
)
BEGIN
    SELECT rt.Revoked, rt.UserId, rt.HashedToken, rt.ExpiryDate, rt.Salt as RefreshSalt, u.FirstName, u.LastName, u.Email, u.Phone, u.Username, u.Salt, u.Password, rt.Complete, rt.VerifyType
    FROM Verification AS rt
    INNER JOIN Users AS u ON rt.UserId = u.Id
    WHERE rt.Id = p_Id AND rt.Revoked = FALSE AND rt.ExpiryDate > NOW()
    AND u.IsActive = 1
    LIMIT 1;
END //
DELIMITER ;