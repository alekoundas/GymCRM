export interface ApiResponse<TEntity> {
  isSucceed: boolean;
  messages: { [key: string]: string[] };
  data?: TEntity;
}

export class ApiResponse<TEntity> implements ApiResponse<TEntity> {
  isSucceed: boolean = true;
  messages: { [key: string]: string[] } = {};
  data?: TEntity;
}
