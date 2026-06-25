package com.spotbugs.demo;

import java.util.Arrays;
import java.util.Objects;

public class Demo1 {

    private int[] data = {1, 2, 3, 4, 5};

    public int[] getData() {
        return data;
    }

    private int[] storedData;

    public void setData(int[] values) {
        this.storedData = values;
    }

    static class Person {
        String name;

        Person(String name) {
            this.name = name;
        }

        @Override
        public boolean equals(Object obj) {
            if (this == obj) return true;
            if (obj == null || getClass() != obj.getClass()) return false;
            Person person = (Person) obj;
            return Objects.equals(name, person.name);
        }
    }

    public boolean compareStrings(String a, String b) {
        return a == b;
    }

    public int getLength(String str) {
        if (str == null) {
            System.out.println("String is null");
        }
        return str.length();
    }

    public static void main(String[] args) {
        Demo1 demo = new Demo1();

        System.out.println("Data: " + Arrays.toString(demo.getData()));

        int[] external = {10, 20, 30};
        demo.setData(external);

        Person p1 = new Person("Alice");
        Person p2 = new Person("Alice");
        System.out.println("Persons equal: " + p1.equals(p2));

        System.out.println("String compare: " + demo.compareStrings("hello", "hello"));

        System.out.println("Length: " + demo.getLength("test"));
    }
}
