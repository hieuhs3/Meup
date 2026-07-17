import type { CapacitorConfig } from '@capacitor/cli';

const isDev = process.env['NODE_ENV'] !== 'production';

const config: CapacitorConfig = {
  appId: 'com.meup.app',
  appName: 'MeUp',
  webDir: 'dist/frontend/browser',

  // Khi dev: trỏ về Angular dev server để hot-reload trên device/emulator
  // Khi build prod: bỏ server block (comment out hoặc xoá)
  ...(isDev && {
    server: {
      url: 'http://10.0.2.2:4200', // Android emulator → localhost host machine
      cleartext: true,
    },
  }),

  plugins: {
    SplashScreen: {
      launchShowDuration: 1500,
      launchAutoHide: true,
      backgroundColor: '#0f141b',
      androidSplashResourceName: 'splash',
      androidScaleType: 'CENTER_CROP',
      showSpinner: false,
      splashFullScreen: true,
      splashImmersive: true,
    },

    StatusBar: {
      style: 'dark',           // dark text trên light bg; dùng 'light' cho dark mode
      backgroundColor: '#0f141b',
      overlaysWebView: false,
    },

    PushNotifications: {
      presentationOptions: ['badge', 'sound', 'alert'],
    },

    Camera: {
      // Không cần config đặc biệt; permissions khai báo trong AndroidManifest / Info.plist
    },
  },

  android: {
    // Cho phép cleartext (http) trong debug — production dùng https
    allowMixedContent: isDev,
    captureInput: true,
    webContentsDebuggingEnabled: isDev,
  },

  ios: {
    contentInset: 'always',    // tránh content bị che bởi notch/home indicator
    scrollEnabled: true,
    limitsNavigationsToAppBoundDomains: true,
  },
};

export default config;
