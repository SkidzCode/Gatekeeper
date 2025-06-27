declare module 'plugins:all' {
  export const pluginLoaders: Record<string, () => Promise<any>>;
}
