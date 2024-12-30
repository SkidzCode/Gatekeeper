DROP PROCEDURE IF EXISTS spDeactivateKey ;
DELIMITER //

CREATE PROCEDURE spDeactivateKey (
    IN p_Id INT
)
BEGIN
    UPDATE KeySecrets 
       SET IsActive = 0
     WHERE Id = p_Id;
END//
DELIMITER ;
