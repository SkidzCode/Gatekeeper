CREATE TABLE IF NOT EXISTS Settings (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ParentId INT NULL,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Category VARCHAR(50),
    SettingValueType ENUM('string', 'integer', 'boolean', 'float', 'json') NOT NULL,
    DefaultSettingValue TEXT NOT NULL,
    SettingValue TEXT NOT NULL,
    CreatedBy INT NOT NULL, 
    UpdatedBy INT NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (ParentId) REFERENCES Settings(Id) ON DELETE SET NULL,
    FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
    FOREIGN KEY (UpdatedBy) REFERENCES Users(Id),
    INDEX idx_parent_id (ParentId),
    INDEX idx_category (Category),
    CHECK (SettingValueType IN ('string', 'integer', 'boolean', 'float', 'json'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
