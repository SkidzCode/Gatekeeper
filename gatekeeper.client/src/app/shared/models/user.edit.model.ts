import { User } from './user.model';
import { Role } from './role.model';
export interface UserEdit{
  user: User;
  roles: Role[];
}
