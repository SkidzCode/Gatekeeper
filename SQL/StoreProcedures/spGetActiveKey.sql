DROP PROCEDURE IF EXISTS spGetActiveKey ;
DELIMITER //
CREATE PROCEDURE spGetActiveKey ()
BEGIN
    SELECT Id,
           SecretKey,
           CreatedDate,
           ExpirationDate,
           IsActive
      FROM KeySecrets
     WHERE IsActive = 1
       AND ExpirationDate > NOW()
     ORDER BY CreatedDate DESC
     LIMIT 1;
END//
DELIMITER ;
