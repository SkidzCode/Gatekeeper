// src/app/models/setting.model.ts

export interface Setting {
  id: number;
  parentId?: number;
  userId?: number;
  name: string;
  category?: string;
  settingValueType: 'string' | 'integer' | 'boolean' | 'float' | 'json';
  defaultSettingValue: string;
  settingValue: string;
  createdBy: number;
  updatedBy: number;
  createdAt: Date;
  updatedAt: Date;
}
