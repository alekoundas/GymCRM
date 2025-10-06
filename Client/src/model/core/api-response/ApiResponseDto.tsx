export interface ApiResponseDto<TEntity> {
  isSucceed: boolean;
  messages: { [key: string]: string[] };
  data?: TEntity;
}

export class ApiResponseDto<TEntity> implements ApiResponseDto<TEntity> {
  isSucceed: boolean = true;
  messages: { [key: string]: string[] } = {};
  data?: TEntity;
}
