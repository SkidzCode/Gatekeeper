DROP PROCEDURE IF EXISTS SearchSettings;
DELIMITER //

CREATE PROCEDURE SearchSettings (
    IN p_Name VARCHAR(100),
    IN p_Category VARCHAR(50),
    IN p_Limit INT,
    IN p_Offset INT
)
BEGIN
    SELECT *
    FROM Settings
    WHERE 
        (p_Name IS NULL OR Name LIKE CONCAT('%', p_Name COLLATE utf8mb4_unicode_ci, '%'))
		AND (p_Category IS NULL OR Category = p_Category) -- Also check collation for Category if it causes issues
    ORDER BY Name
    LIMIT p_Limit OFFSET p_Offset;
END //

DELIMITER ;
