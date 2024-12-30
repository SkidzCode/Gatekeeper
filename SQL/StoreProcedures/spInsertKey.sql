DROP PROCEDURE IF EXISTS spInsertKey ;
DELIMITER //
CREATE PROCEDURE spInsertKey (
    IN p_SecretKey VARBINARY(512),
    IN p_ExpirationDate DATETIME
)
BEGIN
    INSERT INTO KeySecrets (SecretKey, CreatedDate, ExpirationDate, IsActive)
    VALUES (p_SecretKey, NOW(), p_ExpirationDate, 1);
END//
DELIMITER ;
